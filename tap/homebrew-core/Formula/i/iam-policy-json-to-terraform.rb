class IamPolicyJsonToTerraform < Formula
  desc "Convert a JSON IAM Policy into terraform"
  homepage "https://flosell.github.io/iam-policy-json-to-terraform/"
  url "https://github.com/flosell/iam-policy-json-to-terraform/archive/refs/tags/1.9.2.tar.gz"
  sha256 "63a1a8cc21785e70d830b8fc8289c36250d232c432446e39a25c0da2f2f27067"
  license "Apache-2.0"
  head "https://github.com/flosell/iam-policy-json-to-terraform.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "86c624455acea94e72a46bd37f7fcad336af8f5d4edeb1329ffeef629b78c201"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    # test version
    assert_match version.to_s, shell_output("#{bin}/iam-policy-json-to-terraform -version")

    # test functionality
    test_input = '{"Statement":[{"Effect":"Allow","Action":["ec2:Describe*"],"Resource":"*"}]}'
    output = pipe_output(bin/"iam-policy-json-to-terraform", test_input)
    assert_match "ec2:Describe*", output
  end
end
