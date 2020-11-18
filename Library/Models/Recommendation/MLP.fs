// Copyright 2020 The TensorFlow Authors, adapted by the DiffSharp authors. All Rights Reserved.
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

namespace Models

open DiffSharp

/// MLP is a multi-layer perceptron and is used as a component of the DLRM model
type MLP() =
    inherit Model()
    let blocks: Dense[] = [| |]

    /// Randomly initializes a new multilayer perceptron from the given hyperparameters.
    ///
    /// - Parameter dims: Dims represents the size of the input, hidden layers, and output of the
    ///   multi-layer perceptron.
    /// - Parameter sigmoidLastLayer: if `true`, use a `sigmoid` activation function for the last layer,
    ///   `relu` otherwise.
    init(dims: int[], sigmoidLastLayer: bool = false) = 
        for i in 0..<(dims.count-1) do            if sigmoidLastLayer && i = dims.count - 2 then
                blocks.append(Linear(inFeatures=dims[i], outFeatures=dims[i+1], activation= dsharp.sigmoid))
            else
                blocks.append(Linear(inFeatures=dims[i], outFeatures=dims[i+1], activation= dsharp.relu))




    
    override _.forward(input) =
        let blocksReduced = blocks.differentiableReduce(input) =  last, layer in
            layer(last)

        blocksReduced




