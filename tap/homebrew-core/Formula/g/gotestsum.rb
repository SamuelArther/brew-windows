class Gotestsum < Formula
  desc "Human friendly `go test` runner"
  homepage "https://github.com/gotestyourself/gotestsum"
  url "https://github.com/gotestyourself/gotestsum/releases/download/v1.13.0/gotestsum_1.13.0_windows_amd64.tar.gz"
  sha256 "fd5a6dc69e46a0970593e70d85a7e75f16714e9c61d6d72ccc324eb82df5bb8a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gotestyourself/gotestsum/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    bin.install "gotestsum.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gotestsum --version 2>&1")
  end
end
