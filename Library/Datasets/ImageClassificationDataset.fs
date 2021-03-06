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

namespace Datasets
(*
open DiffSharp


/// An image with a label.
type LabeledImage = LabeledData<Tensor<Float>, Tensor (*<int32>*)>

/// Types whose elements represent an image classification dataset (with both
/// training and validation data).
type IImageClassificationData {
  /// The type of the training data, represented as a sequence of epochs, which
  /// are collection of batches.
  associatedtype Training: Sequence 
    where Training.Element: Collection, Training.Element.Element = LabeledImage
  /// The type of the validation data, represented as a collection of batches.
  associatedtype Validation: Collection where Validation.Element = LabeledImage
  /// Creates an instance from a given `batchSize`.
  init(batchSize: int, on device: Device)
  /// The `training` epochs.
  let training: Training { get
  /// The `validation` batches.
  let validation: Validation { get
    
  // The following is probably going to be necessary since we can't extract that
  // information from `Epochs` or `Batches`.
  /// The number of samples in the `training` set.
  //let trainingSampleCount: int {get
  /// The number of samples in the `validation` set.
  //let validationSampleCount: int {get

*)
