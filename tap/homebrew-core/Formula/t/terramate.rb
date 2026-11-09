class Terramate < Formula
  desc "Managing Terraform stacks with change detections and code generations"
  homepage "https://terramate.io/docs/"
  url "https://github.com/terramate-io/terramate/archive/refs/tags/v0.17.4.tar.gz"
  sha256 "3f95c5d24efd57c6af8935ecf22461b17f1a8c9867bfc1269cd8849e19910cfc"
  license "MPL-2.0"
  head "https://github.com/terramate-io/terramate.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "01fe722d7cea80d049a140b1aac6f637ecdbb3ec2dcff1b2d1fcfa3ed0544867"
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
