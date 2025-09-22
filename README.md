# SoftBody

This is a personal project aiming to perform soft body physics in Unity,
A lot of insperation was taken from XPBD: Position-Based Simulation of Compliant Constrained Dynamics \[1\]

A compute shader is utalised to perform calculations (including collisions)

# Demo


<video src="github_resources/soft_ball.mp4" width="320" height="240" controls></video>

<video src="github_resources/softer_ball.mp4" width="320" height="240" controls></video>
# TODO

[] Calculate soft body collisions
[] Make sure that the processing is framerate independent
[] Make code less dependent on magic numbers 


# References

```@inproceedings{10.1145/2994258.2994272,
author = {Macklin, Miles and M\"{u}ller, Matthias and Chentanez, Nuttapong},
title = {XPBD: position-based simulation of compliant constrained dynamics},
year = {2016},
isbn = {9781450345927},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
url = {https://doi.org/10.1145/2994258.2994272},
doi = {10.1145/2994258.2994272},
abstract = {We address the long-standing problem of iteration count and time step dependent constraint stiffness in position-based dynamics (PBD). We introduce a simple extension to PBD that allows it to accurately and efficiently simulate arbitrary elastic and dissipative energy potentials in an implicit manner. In addition, our method provides constraint force estimates, making it applicable to a wider range of applications, such those requiring haptic user-feedback. We compare our algorithm to more expensive non-linear solvers and find it produces visually similar results while maintaining the simplicity and robustness of the PBD method.},
booktitle = {Proceedings of the 9th International Conference on Motion in Games},
pages = {49–54},
numpages = {6},
keywords = {constrained dynamics, physics simulation, position based dynamics},
location = {Burlingame, California},
series = {MIG '16}
}
```