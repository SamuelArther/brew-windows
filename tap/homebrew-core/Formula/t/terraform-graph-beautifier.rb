class TerraformGraphBeautifier < Formula
  desc "CLI to beautify `terraform graph` output"
  homepage "https://github.com/pcasteran/terraform-graph-beautifier"
  url "https://github.com/pcasteran/terraform-graph-beautifier/releases/download/v0.3.4/terraform-graph-beautifier_0.3.4_windows_amd64.tar.gz"
  sha256 "c4763585e47164ddaa3e6c7cae773bb8d5df926c594b7b360546cdeff08c8d56"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pcasteran/terraform-graph-beautifier/archive/refs/tags/v0.3.4.tar.gz"
  end

  def install
    bin.install "terraform-graph-beautifier.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/terraform-graph-beautifier -v 2>&1")
  end
end
