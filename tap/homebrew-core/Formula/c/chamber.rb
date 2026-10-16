class Chamber < Formula
  desc "CLI for managing secrets through AWS SSM Parameter Store"
  homepage "https://github.com/segmentio/chamber"
  url "https://github.com/segmentio/chamber/archive/refs/tags/v3.1.5.tar.gz"
  sha256 "8658307013875044ddb023b5cf221085b4ecdfb3d093929443e636b8d1817e88"
  license "MIT"
  head "https://github.com/segmentio/chamber.git", branch: "master"

  livecheck do
    url :stable
    regex(/v?(\d+(?:\.\d+)+(?:-ci\d)?)/i)
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2ae4124360c972bc3d8d4f57a864b777b287e2a38477d35027232021a3d87754"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=v#{version}")
    generate_completions_from_executable(bin/"chamber", shell_parameter_format: :cobra)
  end

  test do
    ENV["AWS_REGION"] = "us-east-1"
    output = shell_output("#{bin}/chamber list service 2>&1", 1)
    assert_match "Error: Failed to list store contents: operation error SSM", output

    assert_match version.to_s, shell_output("#{bin}/chamber version")
  end
end
