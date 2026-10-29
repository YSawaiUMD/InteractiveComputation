
# Ink Domain — Interactive Boids Simulation

An interactive Unity simulation inspired by Japanese
ink paintings and fantasy combat.

## Overview

The environment contains 12 flocks of animated birds
that move around a circular ink domain.

Each bird follows three Boids rules:
- Separation: avoid nearby birds.
- Alignment: match nearby birds' velocity.
- Cohesion: move toward nearby flockmates.

Additional steering forces control orbiting, attacks,
mouse-guided painting, and domain boundaries.

## Controls

- WASD: Move
- Mouse: Look around
- Shift: Sprint
- Left Click / Space tap: Launch a flock attack
- Hold Left Click / Space: Guide a flock by aiming
- Release attack: Return the flock to its formation
- Escape: Unlock cursor

## Interactive Computation

A quick attack selects an available flock and sends it
through the central target. Other flocks advance around
the orbit to fill the opening.

Holding attack continuously changes a flock's desired
target position using the camera direction. Its movement
still responds to Boids forces.

## Demo

https://youtu.be/X7o9oruvoIY 

## Report
[InteractiveBoidsSimulation.pdf](https://github.com/user-attachments/files/34109788/InteractiveBoidsSimulation.pdf)
