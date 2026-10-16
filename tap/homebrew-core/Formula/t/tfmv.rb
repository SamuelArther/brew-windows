class Tfmv < Formula
  desc "CLI to rename Terraform resources and generate moved blocks"
  homepage "https://github.com/suzuki-shunsuke/tfmv"
  url "https://github.com/suzuki-shunsuke/tfmv/releases/download/v1.0.0/tfmv_windows_amd64.zip"
  sha256 "921ce6cdad7b04d6f4f942a4584001eea0a82915554d30f82ec84e4c1293cc91"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/suzuki-shunsuke/tfmv/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    bin.install "tfmv.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfmv --version 2>&1")
  end
end
