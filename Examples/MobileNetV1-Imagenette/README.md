# MobileNet with Imagenette

This example demonstrates how to train the [MobileNet v1](https://arxiv.org/abs/1704.04861) network against the [Imagenette image classification dataset](https://github.com/fastai/imagenette).

A MobileNet (version 1) network is instantiated from the ImageClassificationModels library of standard models, and applied to an instance of the Imagenette dataset. A custom training loop is defined, and the training and test losses and accuracies for each epoch are shown during training.

## Setup

