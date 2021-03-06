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

type NetD: Layer {
    let module: Sequential<Sequential<Conv2D<Float>, Sequential<Function<Tensor<Float>, Tensor<Float>>, Sequential<Conv2D<Float>, Sequential<BatchNorm<Float>, Sequential<Function<Tensor<Float>, Tensor<Float>>, Sequential<Conv2D<Float>, Sequential<BatchNorm<Float>, Function<Tensor<Float>, Tensor<Float>>>>>>>>>, Sequential<ConvLayer, Sequential<BatchNorm<Float>, Sequential<Function<Tensor<Float>, Tensor<Float>>, ConvLayer>>>>

    public init(inChannels=int, lastConvFilters: int) = 
        let kw = 4

        let module = Sequential {
            Conv2d(filterShape=(kw, kw, inChannels, lastConvFilters),
                          stride=2,
                          padding="same",
                          filterInitializer: { dsharp.randn($0, standardDeviation: dsharp.scalar(0.02)))
            Function<Tensor<Float>, Tensor<Float>> { leakyRelu($0)

            Conv2d(filterShape=(kw, kw, lastConvFilters, 2 * lastConvFilters),
                          stride=2,
                          padding="same",
                          filterInitializer: { dsharp.randn($0, standardDeviation: dsharp.scalar(0.02)))
            BatchNorm(featureCount=2 * lastConvFilters)
            Function<Tensor<Float>, Tensor<Float>> { leakyRelu($0)

            Conv2d(filterShape=(kw, kw, 2 * lastConvFilters, 4 * lastConvFilters),
                          stride=2,
                          padding="same",
                          filterInitializer: { dsharp.randn($0, standardDeviation: dsharp.scalar(0.02)))
            BatchNorm(featureCount=4 * lastConvFilters)
            Function<Tensor<Float>, Tensor<Float>> { leakyRelu($0)


        let module2 = Sequential {
            module
            ConvLayer(inChannels=4 * lastConvFilters, outChannels=8 * lastConvFilters,
                      kernelSize=4, stride=1, padding=1)

            BatchNorm(featureCount=8 * lastConvFilters)
            Function<Tensor<Float>, Tensor<Float>> { leakyRelu($0)

            ConvLayer(inChannels=8 * lastConvFilters, outChannels=1,
                      kernelSize=4, stride=1, padding=1)


        self.module = module2


    
    override _.forward(input) =
        return module(input)


