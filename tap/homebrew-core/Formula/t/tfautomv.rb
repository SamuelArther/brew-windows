class Tfautomv < Formula
  desc "Generate Terraform moved blocks automatically for painless refactoring"
  homepage "https://github.com/busser/tfautomv"
  url "https://github.com/busser/tfautomv/releases/download/v0.7.0/tfautomv_0.7.0_windows_amd64.tar.gz"
  sha256 "4c2da01bd773e9c4017ba7934153c1f0777fed6fe284dd4e73786109cfd5224e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/busser/tfautomv/archive/refs/tags/v0.7.0.tar.gz"
  end

  def install
    bin.install "tfautomv.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfautomv --version 2>&1")
  end
end
