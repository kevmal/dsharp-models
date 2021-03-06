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

namespace Benchmark
(*
open ImageClassificationModels

let WideResNetSuites = [
  makeLayerSuite(
    name= "WideResNet16",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet16)
  },
  makeLayerSuite(
    name= "WideResNet16k10",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet16k10)
  },
  makeLayerSuite(
    name= "WideResNet22",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet22)
  },
  makeLayerSuite(
    name= "WideResNet22k10",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet22k10)
  },
  makeLayerSuite(
    name= "WideResNet28",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet28)
  },
  makeLayerSuite(
    name= "WideResNet28k12",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet28k12)
  },
  makeLayerSuite(
    name= "WideResNet40k1",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet40k1)
  },
  makeLayerSuite(
    name= "WideResNet40k2",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet40k2)
  },
  makeLayerSuite(
    name= "WideResNet40k4",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet40k4)
  },
  makeLayerSuite(
    name= "WideResNet40k8",
    inputDimensions: cifarInput,
    outputDimensions: cifarOutput
  ) = 
    WideResNet(kind: .wideResNet40k8)
  },
]
*)
