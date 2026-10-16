class Mbt < Formula
  desc "Multi-Target Application (MTA) build tool for Cloud Applications"
  homepage "https://sap.github.io/cloud-mta-build-tool"
  url "https://github.com/SAP/cloud-mta-build-tool/releases/download/v1.2.49/cloud-mta-build-tool_1.2.49_Windows_amd64.tar.gz"
  sha256 "413551a06d176de521e857da14e1565f3a3042dc5e50b1c73938a205bf5d907e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/SAP/cloud-mta-build-tool/archive/refs/tags/v1.2.49.tar.gz"
  end

  def install
    bin.install "mbt.exe"
  end

  test do
    assert_match(/generating the "Makefile_\d+.mta" file/, shell_output("#{bin}/mbt build", 1))
    assert_match("Cloud MTA Build Tool", shell_output("#{bin}/mbt --version"))
  end
end
