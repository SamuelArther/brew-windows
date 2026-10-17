class CfnFormat < Formula
  desc "Command-line tool for formatting AWS CloudFormation templates"
  homepage "https://github.com/aws-cloudformation/rain"
  url "https://github.com/aws-cloudformation/rain/archive/refs/tags/v1.24.4.tar.gz"
  sha256 "1387dd8e17160a51e8c99fc6654107bcb39dc94a137338c13e4ade30a344d3ef"
  license "Apache-2.0"
  head "https://github.com/aws-cloudformation/rain.git", branch: "main"

  deprecate! date: "2026-08-08", because: :repo_archived
  disable! date: "2027-02-08", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9717d0278495668d55258e7305e3d980a391511280033784e8299868ff9dcb47"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/cfn-format"
  end

  test do
    (testpath/"test.template").write <<~YAML
      Resources:
        Bucket:
          Type: AWS::S3::Bucket
    YAML
    assert_equal "test.template: formatted OK", shell_output("#{bin}/cfn-format -v test.template").strip
  end
end
