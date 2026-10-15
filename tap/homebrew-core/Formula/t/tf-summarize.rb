class TfSummarize < Formula
  desc "CLI to print the summary of the terraform plan"
  homepage "https://github.com/dineshba/tf-summarize"
  url "https://github.com/dineshba/tf-summarize/releases/download/v0.3.20/tf-summarize_windows_amd64.zip"
  sha256 "4184581ac8887ae7b65552607b3045c448be7fba857617524e5c3f517d588da8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dineshba/tf-summarize/archive/refs/tags/v0.3.20.tar.gz"
  end

  def install
    bin.install "tf-summarize.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tf-summarize -v 2>&1")
  end
end
