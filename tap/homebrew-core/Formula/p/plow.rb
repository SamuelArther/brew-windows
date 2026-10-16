class Plow < Formula
  desc "High-performance and real-time metrics displaying HTTP benchmarking tool"
  homepage "https://deepwiki.com/six-ddc/plow"
  url "https://github.com/six-ddc/plow/releases/download/v1.4.0/plow_1.4.0_windows_amd64.tar.gz"
  sha256 "b617dc04c8aef6039fa249119c668a207145a8350b99b45a6257c7769ce19967"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/six-ddc/plow/archive/refs/tags/v1.4.0.tar.gz"
  end

  def install
    bin.install "plow.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/plow --version 2>&1")
  end
end
