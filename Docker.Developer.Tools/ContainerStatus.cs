namespace Docker.Developer.Tools
{
  public enum ContainerStatus
  {
    unknown = 0,
    created = 1,
    restarting = 2,
    running = 3,
    removing = 4,
    paused = 5,
    exited = 6,
    dead = 7
  }
}
