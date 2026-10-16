class Stern < Formula
  desc "Tail multiple Kubernetes pods & their containers"
  homepage "https://github.com/stern/stern"
  url "https://github.com/stern/stern/releases/download/v1.34.0/stern_1.34.0_windows_amd64.tar.gz"
  sha256 "22ed0c8eb0c15cbd2651c944517f6c06aa6ffa11467be55f7673f7242f1bafd3"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stern/stern/archive/refs/tags/v1.34.0.tar.gz"
  end

  def install
    bin.install "stern.exe"
  end

  test do
    assert_match "version: #{version}", shell_output("#{bin}/stern --version")
  end
end
