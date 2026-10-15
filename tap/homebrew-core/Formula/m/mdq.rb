class Mdq < Formula
  desc "Like jq but for Markdown"
  homepage "https://github.com/yshavit/mdq"
  url "https://github.com/yshavit/mdq/releases/download/v0.10.0/mdq-windows-x64.zip"
  sha256 "5df1bb8b31be594faea2ecea8195c981800acd5753203a25bed332c3dbdf3811"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yshavit/mdq/archive/refs/tags/v0.10.0.tar.gz"
  end

  def install
    bin.install "mdq.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mdq --version 2>&1")
  end
end
