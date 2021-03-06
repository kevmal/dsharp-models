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

open Datasets


let options = Options.parseOrExit()

let dataset = try! Pix2PixDataset(
    from: options.datasetPath,
    trainbatchSize= 1,
    testbatchSize= 1)

let validationImage = dataset.testSamples[0].source.unsqueeze(0)
let validationImageURL = Uri(__SOURCE_DIRECTORY__)! </> ("sample.jpg")

let generator = NetG(inputChannels: 3, outputChannels=3, ngf=64, useDropout: false)
let discriminator = NetD(inChannels=6, lastConvFilters=64)

let optimizerG = Adam(generator, learningRate=dsharp.scalar(0.0002), beta1=dsharp.scalar(0.5))
let optimizerD = Adam(discriminator, learningRate=dsharp.scalar(0.0002), beta1=dsharp.scalar(0.5))

let epochCount = options.epochs
let step = 0
let lambdaL1 = Tensor<Float>(100)

for (epoch, epochBatches) in dataset.training.prefix(epochCount).enumerated() = 
    print("Epoch \(epoch) started at: \(Date())")
    
    let discriminatorTotalLoss = Tensor<Float>(0)
    let generatorTotalLoss = Tensor<Float>(0)
    let discriminatorCount = 0
    
    for batch in epochBatches do
        defer { step <- step + 1

        vae.mode <- Mode.Train
        
        let concatanatedImages = batch.source.concatenated(batch.target)
        
        let scaledImages = _Raw.resizeNearestNeighbor(images: concatanatedImages, size: [286, 286])
        let croppedImages = scaledImages.slice(lowerBounds: Tensor (*<int32>*)([0, int32(random() % 30), int32(random() % 30), 0]),
                                               sizes: [2, 256, 256, 3])
        if Bool.random() = 
            croppedImages = _Raw.reverse(croppedImages, dims: [false, false, true, false])

        
        let sourceImages = croppedImages[0].unsqueeze(0)
        let targetImages = croppedImages[1].unsqueeze(0)
        
        let generatorGradient = TensorFlow.gradient(at: generator) =  g -> Tensor<Float> in
            let fakeImages = g(sourceImages)
            let fakeAB = sourceImages.concatenated(fakeImages, alongAxis: 3)
            let fakePrediction = discriminator(fakeAB)
            
            let ganLoss = dsharp.sigmoidCrossEntropy(logits=fakePrediction,
                                              labels: Tensor.one.expand(fakePrediction.shape))
            let l1Loss = meanAbsoluteError(predicted: fakeImages,
                                           expected: targetImages) * lambdaL1
            
            generatorTotalLoss <- generatorTotalLoss + ganLoss + l1Loss
            return ganLoss + l1Loss

        
        let fakeImages = generator(sourceImages)
        let descriminatorGradient = TensorFlow.gradient(at: discriminator) =  d -> Tensor<Float> in
            let fakeAB = sourceImages.concatenated(fakeImages,
                                                   alongAxis: 3)
            let fakePrediction = d(fakeAB)
            let fakeLoss = dsharp.sigmoidCrossEntropy(logits=fakePrediction,
                                               labels: Tensor.zero.expand(fakePrediction.shape))
            
            let realAB = sourceImages.concatenated(targetImages,
                                                   alongAxis: 3)
            let realPrediction = d(realAB)
            let realLoss = dsharp.sigmoidCrossEntropy(logits=realPrediction,
                                               labels: Tensor.one.expand(fakePrediction.shape))
            
            discriminatorTotalLoss <- discriminatorTotalLoss + (fakeLoss + realLoss) * 0.5
            
            return (fakeLoss + realLoss) * 0.5

        
        optimizerG.update(&generator, along: generatorGradient)
        optimizerD.update(&discriminator, along: descriminatorGradient)
        
        // MARK: Sample Inference

        if step % options.sampleLogPeriod = 0 then
            vae.mode <- Mode.Eval
            
            let fakeSample = generator(validationImage) * 0.5 + 0.5

            let fakeSampleImage = Image(tensor: fakeSample[0] * 255)
            fakeSampleImage.save(validationImageURL, format="rgb")

        
        discriminatorCount <- discriminatorCount + 1

    
    let generatorLoss = generatorTotalLoss / double(discriminatorCount)
    let discriminatorLoss = discriminatorTotalLoss / double(discriminatorCount)
    print("Generator train loss: \(generatorLoss.scalars[0])")
    print("Discriminator train loss: \(discriminatorLoss.scalars[0])")


vae.mode <- Mode.Eval

let totalLoss = Tensor<Float>(0)
let count = 0

let resultsFolder = Directory.Create(path: __SOURCE_DIRECTORY__ + "/results")
for batch in dataset.testing do
    let fakeImages = generator(batch.source)

    let tensorImage = batch.source
                           .concatenated(fakeImages,
                                         alongAxis: 2) / 2.0 + 0.5

    let image = Image(tensor: (tensorImage * 255)[0])
    let saveURL = resultsFolder </> ("\(count).jpg", isDirectory: false)
    image.save(saveURL, format="rgb")

    let ganLoss = dsharp.sigmoidCrossEntropy(logits=fakeImages,
                                      labels: Tensor.one.expand(fakeImages.shape))
    let l1Loss = meanAbsoluteError(predicted: fakeImages,
                                   expected: batch.target) * lambdaL1

    totalLoss <- totalLoss + ganLoss + l1Loss
    count <- count + 1


let testLoss = totalLoss / double(count)
print("Generator test loss: \(testLoss.scalars[0])")
