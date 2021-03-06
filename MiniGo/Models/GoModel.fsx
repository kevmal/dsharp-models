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

// Implements the same architecture as https://github.com/tensorflow/minigo/blob/master/dual_net.py

open DiffSharp

type ModelConfiguration {
    /// The size of the Go board (typically `9` or `19`).
    let boardSize: int
    /// The number of output features of conv layers in shared trunk.
    let convWidth: int
    /// The output feature count of conv layer in policy head.
    let policyConvWidth: int
    /// The output feature count of conv layer in value head.
    let valueConvWidth: int
    /// The output feature count of dense layer in value head.
    let valueDenseWidth: int
    /// The number of layers (typically equal to `boardSize`).
    let layerCount: int

    public init(boardSize: int) = 
        self.boardSize = boardSize
        self.convWidth = boardSize = 19 ? 256 : 32
        self.policyConvWidth = 2
        self.valueConvWidth = 1
        self.valueDenseWidth = boardSize = 19 ? 256 : 64
        self.layerCount = boardSize



type ConvBN: Layer {
    let conv: Conv2D<Float>
    let norm: BatchNorm<Float>

    init(
        filterShape=(Int, Int, Int, Int),
        strides = [Int, Int) = (1, 1),
        padding: Padding,
        bias: bool = true,
        affine: bool = true
    ) = 
        // TODO(jekbradbury): thread through bias and affine boolean arguments
        // (behavior is correct for inference but this should be changed for training)
        self.conv = Conv2d(filterShape: filterShape, strides: strides, padding: padding)
        self.norm = BatchNorm(featureCount=filterShape.3, momentum: 0.95, epsilon: 1e-5)


    
    override _.forward(input) =
        return norm(conv(input))



extension ConvBN: LoadableFromPythonCheckpoint {
    mutating let load(from reader: MiniGoCheckpointReader) = 
        conv.load(reader)
        norm.load(reader)



type ResidualIdentityBlock: Layer {
    let layer1: ConvBN
    let layer2: ConvBN

    public init(featureCounts: (Int, Int), kernelSize: int = 3) = 
        self.layer1 = ConvBN(
            filterShape=(kernelSize, kernelSize, featureCounts.0, featureCounts.1),
            padding="same",
            bias: false)

        self.layer2 = ConvBN(
            filterShape=(kernelSize, kernelSize, featureCounts.1, featureCounts.1),
            padding="same",
            bias: false)


    
    override _.forward(input) =
        let tmp = relu(layer1(input))
        tmp = layer2(tmp)
        return relu(tmp + input)



extension ResidualIdentityBlock: LoadableFromPythonCheckpoint {
    mutating let load(from reader: MiniGoCheckpointReader) = 
        layer1.load(reader)
        layer2.load(reader)



// This is needed because we can't conform tuples to protocols
type GoModelOutput: Differentiable {
    let policy: Tensor
    let value: Tensor
    let logits: Tensor


type GoModel: Layer {
    let configuration: ModelConfiguration
    let initialConv: ConvBN
    let residualBlocks: [ResidualIdentityBlock]
    let policyConv: ConvBN
    let policyDense: Dense
    let valueConv: ConvBN
    let valueDense1: Dense
    let valueDense2: Dense

    public init(configuration: ModelConfiguration) = 
        self.configuration = configuration
        
        initialConv = ConvBN(
            filterShape=(3, 3, 17, configuration.convWidth),
            padding="same",
            bias: false)
        residualBlocks = (1...configuration.boardSize).map { _ in
            ResidualIdentityBlock(featureCounts: (configuration.convWidth, configuration.convWidth))

        policyConv = ConvBN(
            filterShape=(1, 1, configuration.convWidth, configuration.policyConvWidth),
            padding="same",
            bias: false,
            affine: false)
        policyDense = Dense(
            inputSize= configuration.policyConvWidth * configuration.boardSize
                * configuration.boardSize,
            outputSize=configuration.boardSize * configuration.boardSize + 1,
            activation= {$0)
        valueConv = ConvBN(
            filterShape=(1, 1, configuration.convWidth, configuration.valueConvWidth),
            padding="same",
            bias: false,
            affine: false)
        valueDense1 = Dense(
            inputSize= configuration.valueConvWidth * configuration.boardSize
                * configuration.boardSize,
            outputSize=configuration.valueDenseWidth,
            activation= relu)
        valueDense2 = Dense(
            inputSize= configuration.valueDenseWidth,
            outputSize=1,
            activation= tanh)

  
    (wrt: (self, input))
    override _.forward(input: Tensor) = GoModelOutput {
        let batchSize = input.shape.[0]
        let output = relu(initialConv(input))

        for i in 0..<configuration.boardSize {
            output = residualBlocks[i](output)


        let policyConvOutput = relu(policyConv(output))
        let logits = policyDense(policyConvOutput.reshape(to:
            [batchSize,
             configuration.policyConvWidth * configuration.boardSize * configuration.boardSize]))
        let policyOutput = softmax(logits)

        let valueConvOutput = relu(valueConv(output))
        let valueHidden = valueDense1(valueConvOutput.reshape(to:
            [batchSize,
             configuration.valueConvWidth * configuration.boardSize * configuration.boardSize]))
        let valueOutput = valueDense2(valueHidden).reshape([batchSize])

        return GoModelOutput(policy: policyOutput, value: valueOutput, logits: logits)


    @usableFromInline
    @derivative(of: callAsFunction, wrt: (self, input))
    let _vjpCall(input: Tensor)
        -> (value: GoModelOutput, pullback: (GoModelOutput.TangentVector)
        -> (GoModel.TangentVector, Tensor<Float>)) = 
        // TODO(jekbradbury): add a real VJP
        // (we're only interested in inference for now and have control flow in our `call(_:)` method)
        return (self(input), {
            seed in (GoModel.TangentVector.zero, Tensor<Float>(0))
)



extension GoModel: InferenceModel {
    let prediction(for input: Tensor) = GoModelOutput {
        return self(input)



extension GoModel: LoadableFromPythonCheckpoint {
    public mutating let load(from reader: MiniGoCheckpointReader) = 
        initialConv.load(reader)
        for i in 0..<configuration.boardSize {
            residualBlocks[i].load(reader)


        // Special-case the two batchnorms that lack affine weights.
        policyConv.conv.load(reader)
        policyConv.norm.runningMean.value = reader.readTensor(
            layerName: "batch_normalization",
            weightName: "moving_mean")!
        policyConv.norm.runningVariance.value = reader.readTensor(
            layerName: "batch_normalization",
            weightName: "moving_variance")!
        reader.increment(layerName: "batch_normalization")

        policyDense.load(reader)

        valueConv.conv.load(reader)
        valueConv.norm.runningMean.value = reader.readTensor(
            layerName: "batch_normalization",
            weightName: "moving_mean")!
        valueConv.norm.runningVariance.value = reader.readTensor(
            layerName: "batch_normalization",
            weightName: "moving_variance")!
        reader.increment(layerName: "batch_normalization")

        valueDense1.load(reader)
        valueDense2.load(reader)


