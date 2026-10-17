class S3scanner < Formula
  desc "Scan for misconfigured S3 buckets across S3-compatible APIs!"
  homepage "https://github.com/sa7mon/S3Scanner"
  url "https://github.com/sa7mon/S3Scanner/archive/refs/tags/v3.1.1.tar.gz"
  sha256 "2d333c31909baa21e024d11db1b03647fff3d210d73fa7fa47f598d3d459a20c"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b2768efe68f7729bb362e041f52caf81d95a062ca73325f889ff4c4d3c90cfc1"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=v#{version}")
  end

  test do
    version_output = shell_output("#{bin}/s3scanner --version")
    assert_match version.to_s, version_output

    # test that scanning our private bucket returns:
    #  - bucket exists
    #  - bucket does not allow anonymous user access
    private_output = shell_output("#{bin}/s3scanner -bucket s3scanner-private")
    assert_includes private_output, "exists"
    assert_includes private_output, "AuthUsers: [] | AllUsers: []"
  end
end
