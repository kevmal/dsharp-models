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

open DiffSharp

// Original Paper:
// "Very Deep Convolutional Networks for Large-Scale Image Recognition"
// Karen Simonyan, Andrew Zisserman
// https://arxiv.org/abs/1409.1556

type VGGBlock: Layer {
    let blocks: [Conv2D<Float>] = []
    let maxpool = MaxPool2D<Float>(poolSize: (2, 2), stride=2)

    public init(featureCounts: (Int, Int, Int, Int), blockCount: int) = 
        self.blocks = [Conv2d(filterShape=(3, 3, featureCounts.0, featureCounts.1),
            padding="same",
            activation= relu)]
        for _ in 1..<blockCount {
            self.blocks <- blocks + [Conv2d(filterShape=(3, 3, featureCounts.2, featureCounts.3),
                padding="same",
                activation= relu)]



    
    override _.forward(input) =
        return maxpool(blocks.differentiableReduce(input) =  $1($0))



type VGG16: Layer {
    let layer1: VGGBlock
    let layer2: VGGBlock
    let layer3: VGGBlock
    let layer4: VGGBlock
    let layer5: VGGBlock

    let flatten = Flatten()
    let dense1 = Dense(inputSize=512 * 7 * 7, outputSize=4096, activation= relu)
    let dense2 = Dense(inputSize=4096, outputSize=4096, activation= relu)
    let output: Dense

    public init(classCount: int = 1000) = 
        layer1 = VGGBlock(featureCounts: (3, 64, 64, 64), blockCount: 2)
        layer2 = VGGBlock(featureCounts: (64, 128, 128, 128), blockCount: 2)
        layer3 = VGGBlock(featureCounts: (128, 256, 256, 256), blockCount: 3)
        layer4 = VGGBlock(featureCounts: (256, 512, 512, 512), blockCount: 3)
        layer5 = VGGBlock(featureCounts: (512, 512, 512, 512), blockCount: 3)
        output = Dense(inputSize=4096, outputSize=classCount)


    
    override _.forward(input) =
        let backbone = input |> layer1, layer2, layer3, layer4, layer5)
        return backbone |> flatten, dense1, dense2, output)



type VGG19: Layer {
    let layer1: VGGBlock
    let layer2: VGGBlock
    let layer3: VGGBlock
    let layer4: VGGBlock
    let layer5: VGGBlock

    let flatten = Flatten()
    let dense1 = Dense(inputSize=512 * 7 * 7, outputSize=4096, activation= relu)
    let dense2 = Dense(inputSize=4096, outputSize=4096, activation= relu)
    let output: Dense

    public init(classCount: int = 1000) = 
        layer1 = VGGBlock(featureCounts: (3, 64, 64, 64), blockCount: 2)
        layer2 = VGGBlock(featureCounts: (64, 128, 128, 128), blockCount: 2)
        layer3 = VGGBlock(featureCounts: (128, 256, 256, 256), blockCount: 4)
        layer4 = VGGBlock(featureCounts: (256, 512, 512, 512), blockCount: 4)
        layer5 = VGGBlock(featureCounts: (512, 512, 512, 512), blockCount: 4)
        output = Dense(inputSize=4096, outputSize=classCount)


    
    override _.forward(input) =
        let backbone = input |> layer1, layer2, layer3, layer4, layer5)
        return backbone |> flatten, dense1, dense2, output)


