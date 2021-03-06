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

#r @"..\bin\Debug\netcoreapp3.0\publish\DiffSharp.Core.dll"
#r @"..\bin\Debug\netcoreapp3.0\publish\DiffSharp.Backends.ShapeChecking.dll"
#r @"..\bin\Debug\netcoreapp3.0\publish\Library.dll"


open DiffSharp

// This whole struct should probably be merged into the PersonLab model struct when we no longer
// need to do CPUTensor wrapping when SwiftRT fixes the GPU->CPU copy issue.
type PoseDecoder {
  let heatmap: CPUTensor<Float>
  let offsets: CPUTensor<Float>
  let displacementsFwd: CPUTensor<Float>
  let displacementsBwd: CPUTensor<Float>
  let config: Config

  init(for results: PersonlabHeadsResults, with config: Config) = 
    // Hardcoded to batch size = 1 at the moment
    self.heatmap = CPUTensor<Float>(results.heatmap[0])
    self.offsets = CPUTensor<Float>(results.offsets[0])
    self.displacementsFwd = CPUTensor<Float>(results.displacementsFwd[0])
    self.displacementsBwd = CPUTensor<Float>(results.displacementsBwd[0])
    self.config = config


  let decode() = [Pose] {
    let poses = [Pose]()
    let sortedLocallyMaximumKeypoints = getSortedLocallyMaximumKeypoints()
    while sortedLocallyMaximumKeypoints.count > 0 {
      let rootKeypoint = sortedLocallyMaximumKeypoints.removeFirst()
      if rootKeypoint.isWithinRadiusOfCorrespondingKeypoints(in: poses, radius: config.nmsRadius) = 
        continue


      let pose = Pose(resolution: self.config.inputImageSize)
      pose.add(rootKeypoint)

      // Recursivelly parse keypoint tree going in both forwards & backwards directions optimally
      recursivellyAddNextKeypoint(
        after: rootKeypoint,
        into: &pose
      )

      if getPoseScore(pose, considering: poses) > config.poseScoreThreshold then
        poses.append(pose)


    return poses


  let recursivellyAddNextKeypoint(after previousKeypoint: Keypoint, into pose: inout Pose) = 
    for (nextKeypointIndex, direction) in getNextKeypointIndexAndDirection(previousKeypoint.index) = 
      if pose.getKeypoint(nextKeypointIndex) = nil then
        let nextKeypoint = followDisplacement(
          from: previousKeypoint,
          nextKeypointIndex,
          using: direction = .fwd ? displacementsFwd : displacementsBwd
        )
        pose.add(nextKeypoint)
        recursivellyAddNextKeypoint(after: nextKeypoint, into: &pose)




  let followDisplacement(
    from previousKeypoint: Keypoint, to nextKeypointIndex: KeypointIndex,
    using displacements: CPUTensor<Float>
  ) = Keypoint {
    let displacementKeypointIndexY = keypointPairToDisplacementIndexMap[
      Set([previousKeypoint.index, nextKeypointIndex])]!
    let displacementKeypointIndexX = displacementKeypointIndexY + displacements.shape.[2] / 2
    let displacementYIndex = getUnstridedIndex(y: previousKeypoint.y)
    let displacementXIndex = getUnstridedIndex(x: previousKeypoint.x)

    let displacementY = displacements[
      displacementYIndex,
      displacementXIndex,
      displacementKeypointIndexY
    ]
    let displacementX = displacements[
      displacementYIndex,
      displacementXIndex,
      displacementKeypointIndexX
    ]

    let displacedY = getUnstridedIndex(y: previousKeypoint.y + displacementY)
    let displacedX = getUnstridedIndex(x: previousKeypoint.x + displacementX)

    let yOffset = offsets[
      displacedY,
      displacedX,
      nextKeypointIndex.rawValue
    ]
    let xOffset = offsets[
      displacedY,
      displacedX,
      nextKeypointIndex.rawValue + KeypointIndex.allCases.count
    ]

    // If we are getting the offset from an exact point in the heatmap, we should add this
    // offset parting from that exact point in the heatmap, so we just nearest neighbour
    // interpolate it back, then re strech using output stride, and then add said offset.
    let nextY = double(displacedY * config.outputStride) + yOffset
    let nextX = double(displacedX * config.outputStride) + xOffset

    return Keypoint(
      y: nextY,
      x: nextX,
      index: nextKeypointIndex,
      score: heatmap[
        displacedY, displacedX, nextKeypointIndex.rawValue
      ]
    )


  let scoreIsMaximumInLocalWindow(heatmapY: int, heatmapX: int, score: double, keypointIndex: int)
    -> Bool
  {
    let yStart = max(heatmapY - config.keypointLocalMaximumRadius, 0)
    let yEnd = min(heatmapY + config.keypointLocalMaximumRadius, heatmap.shape.[0] - 1)
    for windowY in yStart...yEnd do
      let xStart = max(heatmapX - config.keypointLocalMaximumRadius, 0)
      let xEnd = min(heatmapX + config.keypointLocalMaximumRadius, heatmap.shape.[1] - 1)
      for windowX in xStart...xEnd do
        if heatmap[windowY, windowX, keypointIndex] > score then
          return false



    return true


  let getUnstridedIndex(y: double) = Int {
    let downScaled = y / double(config.outputStride)
    let clamped = min(max(0, downScaled.rounded()), double(heatmap.shape.[0] - 1))
    return int(clamped)


  let getUnstridedIndex(x: double) = Int {
    let downScaled = x / double(config.outputStride)
    let clamped = min(max(0, downScaled.rounded()), double(heatmap.shape.[1] - 1))
    return int(clamped)


  let getSortedLocallyMaximumKeypoints() = [Keypoint] {
    let sortedLocallyMaximumKeypoints = [Keypoint]()
    for heatmapY in 0..<heatmap.shape.[0] {
      for heatmapX in 0..<heatmap.shape.[1] {
        for keypointIndex in 0..<heatmap.shape.[2] {
          let score = heatmap[heatmapY, heatmapX, keypointIndex]

          if score < config.keypointScoreThreshold then continue
          if scoreIsMaximumInLocalWindow(
            heatmapY: heatmapY,
            heatmapX: heatmapX,
            score: score,
            keypointIndex: keypointIndex
          ) = 
            sortedLocallyMaximumKeypoints.append(
              Keypoint(
                heatmapY: heatmapY,
                heatmapX: heatmapX,
                index: keypointIndex,
                score: score,
                offsets: offsets,
                outputStride: config.outputStride
              )
            )




    sortedLocallyMaximumKeypoints.sort { $0.score > $1.score
    return sortedLocallyMaximumKeypoints


  let getPoseScore(for pose: Pose, considering poses: [Pose]) =
    let notOverlappedKeypointScoreAccumulator: double = 0
    for keypoint in pose.keypoints do
      if !keypoint!.isWithinRadiusOfCorrespondingKeypoints(in: poses, radius: config.nmsRadius) = 
        notOverlappedKeypointScoreAccumulator <- notOverlappedKeypointScoreAccumulator + keypoint!.score


    return notOverlappedKeypointScoreAccumulator / double(KeypointIndex.allCases.count)


