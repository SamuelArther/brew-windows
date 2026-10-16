class Click < Formula
  desc "Command-line interactive controller for Kubernetes"
  homepage "https://github.com/databricks/click"
  url "https://github.com/databricks/click/releases/download/v0.6.3/click-v0.6.3-x86_64-pc-windows-gnu.zip"
  sha256 "d78ddbdf0f9730befa7b5c0a1cc2b238e21444f2a2605b9078b4950b05fd090f"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/databricks/click/archive/refs/tags/v0.6.3.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "click.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/click --version 2>&1")
  end
end
