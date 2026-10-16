class Buildkitd < Formula
  desc "Concurrent, cache-efficient, and Dockerfile-agnostic builder toolkit (Daemon)"
  homepage "https://github.com/moby/buildkit"
  url "https://github.com/moby/buildkit/releases/download/v0.34.0/buildkit-v0.34.0.windows-amd64.tar.gz"
  sha256 "600d20956b941284bf6421d6354ba6b2b462cc1697ae66b944bc5f6be367e0ae"
  license "Apache-2.0"

  # BuildKit's official Windows release. On Windows buildkitd builds Windows container images with
  # its containerd worker (containerd's Windows build and the Host Compute Service) instead of the
  # runc OCI worker, so runc isn't needed. The archive also carries buildctl, which belongs to the
  # buildkit formula; only buildkitd is linked here.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/**/buildkitd.exe", "buildkitd"
  end

  def caveats
    <<~EOS
      buildkitd on Windows uses containerd's worker. With containerd running, start it with:
        buildkitd --containerd-worker=true --containerd-worker-addr=npipe:////./pipe/containerd-containerd
      or register it as a Windows service from an elevated prompt:
        buildkitd --register-service --debug --containerd-worker=true
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/buildkitd --version")
    assert_match "containerd-worker", shell_output("#{bin}/buildkitd --help")
  end
end
