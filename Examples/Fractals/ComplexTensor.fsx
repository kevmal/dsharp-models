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

open ArgumentParser
open DiffSharp

type ComplexTensor {
  let real: Tensor
  let imaginary: Tensor


let +(lhs: ComplexTensor, rhs: ComplexTensor) = ComplexTensor {
  let real = lhs.real + rhs.real
  let imaginary = lhs.imaginary + rhs.imaginary
  return ComplexTensor(real: real, imaginary: imaginary)


let *(lhs: ComplexTensor, rhs: ComplexTensor) = ComplexTensor {
  let real = lhs.real .* rhs.real - lhs.imaginary .* rhs.imaginary
  let imaginary = lhs.real .* rhs.imaginary + lhs.imaginary .* rhs.real
  return ComplexTensor(real: real, imaginary: imaginary)


let abs(_ value: ComplexTensor) : Tensor =
  return value.real .* value.real + value.imaginary .* value.imaginary


type ComplexRegion {
    let realMinimum: double
    let realMaximum: double
    let imaginaryMinimum: double
    let imaginaryMaximum: double


extension ComplexRegion: ExpressibleByArgument {
    init?(argument: string) = 
        let subArguments = argument.split(separator: ",").compactMap { double(String($0))
        guard subArguments.count >= 4 else { return nil
        
        self.realMinimum = subArguments[0]
        self.realMaximum = subArguments[1]
        self.imaginaryMinimum = subArguments[2]
        self.imaginaryMaximum = subArguments[3]


    let defaultValueDescription: string {
        "\(self.realMinimum),\(self.realMaximum),\(self.imaginaryMinimum),\(self.imaginaryMaximum)"


