class DockerEngine < Formula
  desc "Pack, ship and run any application as a lightweight container (Daemon)"
  homepage "https://www.docker.com/"
  url "https://download.docker.com/win/static/stable/x86_64/docker-29.9.1.zip"
  sha256 "5d9b66509390553fd97d590ec2ff0ce10b04c2ac653fa52f2abf2f05c4c197ca"
  license "Apache-2.0"

  # Docker's official static Windows build of the engine (the same moby source, built for Windows).
  # On Windows dockerd runs Windows containers itself through the Host Compute Service (process or
  # Hyper-V isolation), so containerd, runc, tini and nftables aren't needed. The zip also carries
  # the docker CLI, which belongs to the docker formula; only dockerd is linked here.

  livecheck do
    url "https://download.docker.com/win/static/stable/x86_64/"
    regex(/href=.*?docker[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  def install
    install_tree
    windows_exe "libexec/**/dockerd.exe", "dockerd"
  end

  def caveats
    <<~EOS
      dockerd needs Windows' Containers feature (and Hyper-V for --isolation=hyperv):
        Enable-WindowsOptionalFeature -Online -FeatureName Containers -All
      Register and start it as a Windows service from an elevated prompt:
        dockerd --register-service
        Start-Service docker
    EOS
  end

  test do
    assert_match "Docker version #{version}", shell_output("#{bin}/dockerd --version")
    assert_match "--register-service", shell_output("#{bin}/dockerd --help")
  end
end
