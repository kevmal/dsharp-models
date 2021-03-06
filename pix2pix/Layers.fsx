// Copyright 2019 The TensorFlow Authors, adapted by the DiffSharp authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#r @"..\bin\Debug\netcoreapp3.0\publish\DiffSharp.Core.dll"
#r @"..\bin\Debug\netcoreapp3.0\publish\DiffSharp.Backends.ShapeChecking.dll"
#r @"..\bin\Debug\netcoreapp3.0\publish\Library.dll"

open DiffSharp


type Identity: ParameterlessLayer {
    type TangentVector = EmptyTangentVector

    
    override _.forward(input) =
        input



/// 2-D layer applying instance normalization over a mini-batch of inputs.
///
/// Reference: [Instance Normalization](https://arxiv.org/abs/1607.08022)
type InstanceNorm2D<Scalar: TensorFlowFloatingPoint>: Layer {
    /// Learnable parameter scale for affine transformation.
    let scale: Tensor<Scalar>
    /// Learnable parameter offset for affine transformation.
    let offset: Tensor<Scalar>
    /// Small value added in denominator for numerical stability.
    let epsilon: Tensor<Scalar>

    /// Creates a instance normalization 2D Layer.
    ///
    /// - Parameters:
    ///   - featureCount: Size of the channel axis in the expected input.
    ///   - epsilon: Small scalar added for numerical stability.
    public init(featureCount=int, epsilon: Tensor<Scalar> = dsharp.tensor(1e-5)) = 
        self.epsilon = epsilon
        scale = Tensor<Scalar>(ones: [featureCount])
        offset = Tensor<Scalar>(zeros: [featureCount])


    /// Returns the output obtained from applying the layer to the given input.
    ///
    /// - Parameter input: The input to the layer. Expected input layout is BxHxWxC.
    /// - Returns: The output.
    
    override _.forward(input: Tensor<Scalar>) : Tensor =
        // Calculate mean & variance along H,W axes.
        let mean = input.mean(dim=[1; 2])
        let variance = input.variance(dim=[1; 2])
        let norm = (input - mean) * rsqrt(variance + epsilon)
        return norm * scale + offset



type ConvLayer: Layer {
    type Input = Tensor<Float>
    type Output = Tensor<Float>

    /// Padding layer.
    let pad: ZeroPadding2D<Float>
    /// Convolution layer.
    let conv2d: Conv2D<Float>

    /// Creates 2D convolution with padding layer.
    ///
    /// - Parameters:
    ///   - inChannels=Number of input channels in convolution kernel.
    ///   - outChannels: Number of output channels in convolution kernel.
    ///   - kernelSize: Convolution kernel size (both width and height).
    ///   - stride: Stride size (both width and height).
    public init(inChannels=int, outChannels: int, kernelSize: int, stride: int, padding: int? = nil) = 
        let _padding =  padding ?? int(kernelSize / 2)
        pad = ZeroPadding2D(padding: ((_padding, _padding), (_padding, _padding)))
    
        conv2d = Conv2d(filterShape=(kernelSize, kernelSize, inChannels, outChannels),
                        strides = [stride, stride),
                        filterInitializer: { dsharp.randn($0, standardDeviation: dsharp.scalar(0.02)))


    /// Returns the output obtained from applying the layer to the given input.
    ///
    /// - Parameter input: The input to the layer.
    /// - Returns: The output.
    
    override _.forward(input: Input) = Output {
        return input |> pad, conv2d)



type UNetSkipConnectionInnermost: Layer {
    let downConv: Conv2D<Float>
    let upConv: ConvTranspose2d
    let upNorm: BatchNorm<Float>
    
    public init(inChannels=int,
                innerChannels: int,
                outChannels: int) = 
        self.downConv = Conv2d(filterShape=(4, 4, inChannels, innerChannels),
                               stride=2,
                               padding="same",
                               filterInitializer: { dsharp.randn($0,
                                                            standardDeviation: dsharp.scalar(0.02)))
        self.upNorm = BatchNorm(featureCount=outChannels)
        
        self.upConv = ConvTranspose2d(filterShape=(4, 4, innerChannels, outChannels),
                                       stride=2,
                                       padding="same",
                                       filterInitializer: { dsharp.randn($0,
                                                                    standardDeviation: dsharp.scalar(0.02)))

    
    
    override _.forward(input) =
        let x = leakyRelu(input)
        x = self.downConv(x)
        x = relu(x)
        x = x |> self.upConv, self.upNorm)

        return input.concatenated(x, alongAxis: 3)




type UNetSkipConnection<Sublayer: Layer>: Layer where Sublayer.TangentVector.VectorSpaceScalar = Float, Sublayer.Input = Tensor<Float>, Sublayer.Output =: Tensor =
    let downConv: Conv2D<Float>
    let downNorm: BatchNorm<Float>
    let upConv: ConvTranspose2d
    let upNorm: BatchNorm<Float>
    let dropOut = Dropout2d(p=0.5)
    let useDropOut: bool
    
    let submodule: Sublayer
    
    public init(inChannels=int,
                innerChannels: int,
                outChannels: int,
                submodule: Sublayer,
                useDropOut: bool = false) = 
        self.downConv = Conv2d(filterShape=(4, 4, inChannels, innerChannels),
                               stride=2,
                               padding="same",
                               filterInitializer: { dsharp.randn($0, standardDeviation: dsharp.scalar(0.02)))
        self.downNorm = BatchNorm(featureCount=innerChannels)
        self.upNorm = BatchNorm(featureCount=outChannels)
        
        self.upConv = ConvTranspose2d(filterShape=(4, 4, outChannels, innerChannels * 2),
                                       stride=2,
                                       padding="same",
                                       filterInitializer: { dsharp.randn($0,
                                                                    standardDeviation: dsharp.scalar(0.02)))
    
        self.submodule = submodule
        
        self.useDropOut = useDropOut

    
    
    override _.forward(input) =
        let x = leakyRelu(input)
        x = x |> self.downConv, self.downNorm, self.submodule)
        x = relu(x)
        x = x |> self.upConv, self.upNorm)
        
        if self.useDropOut then
            x = self.dropOut(x)

        
        return input.concatenated(x, alongAxis: 3)



type UNetSkipConnectionOutermost<Sublayer: Layer>: Layer where Sublayer.TangentVector.VectorSpaceScalar = Float, Sublayer.Input = Tensor<Float>, Sublayer.Output =: Tensor =
    let downConv: Conv2D<Float>
    let upConv: ConvTranspose2d
    
    let submodule: Sublayer
    
    public init(inChannels=int,
                innerChannels: int,
                outChannels: int,
                submodule: Sublayer) = 
        self.downConv = Conv2d(filterShape=(4, 4, inChannels, innerChannels),
                               stride=2,
                               padding="same",
                               filterInitializer: { dsharp.randn($0,
                                                            standardDeviation: dsharp.scalar(0.02)))
        self.upConv = ConvTranspose2d(filterShape=(4, 4, outChannels, innerChannels * 2),
                                       stride=2,
                                       padding="same",
                                       activation= tanh,
                                       filterInitializer: { dsharp.randn($0,
                                                                    standardDeviation: dsharp.scalar(0.02)))
    
        self.submodule = submodule

    
    
    override _.forward(input) =
        let x = input |> self.downConv, self.submodule)
        x = relu(x)
        x = self.upConv(x)

        return x


