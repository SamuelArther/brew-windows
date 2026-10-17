class AwsConsole < Formula
  desc "Command-line to use AWS CLI credentials to launch the AWS console in a browser"
  homepage "https://github.com/aws-cloudformation/rain"
  url "https://github.com/aws-cloudformation/rain/archive/refs/tags/v1.24.4.tar.gz"
  sha256 "1387dd8e17160a51e8c99fc6654107bcb39dc94a137338c13e4ade30a344d3ef"
  license "Apache-2.0"
  head "https://github.com/aws-cloudformation/rain.git", branch: "main"

  deprecate! date: "2026-08-08", because: :repo_archived
  disable! date: "2027-02-08", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "17d4a202fae6cbe0b71756698484277e42a4fc88052257987fe06f12b64158c3"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/aws-console"

    generate_completions_from_executable(bin/"aws-console", shell_parameter_format: :cobra)
  end

  test do
    # No other operation is possible without valid AWS credentials configured
    output = shell_output("#{bin}/aws-console 2>&1", 1)
    assert_match "a region was not specified. You can run 'aws configure' or choose a profile with a region", output
  end
end
