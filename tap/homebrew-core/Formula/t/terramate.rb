class Terramate < Formula
  desc "Managing Terraform stacks with change detections and code generations"
  homepage "https://terramate.io/docs/"
  url "https://github.com/terramate-io/terramate/archive/refs/tags/v0.17.3.tar.gz"
  sha256 "b783e10d2045401fa42187b58a759d8dd75035ef9811befe9c4a4f788bddf61e"
  license "MPL-2.0"
  head "https://github.com/terramate-io/terramate.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ba9327140abead5bf35f2da43652b01fb9f9bfec56cfbc116337710be66fffab"
  end

  depends_on "go" => :build

  conflicts_with "tenv", because: "both install terramate binary"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"terramate"), "./cmd/terramate"
    system "go", "build", *std_go_args(output: bin/"terramate-ls"), "./cmd/terramate-ls"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/terramate version")
    assert_match version.to_s, shell_output("#{bin}/terramate-ls -version")
  end
end
