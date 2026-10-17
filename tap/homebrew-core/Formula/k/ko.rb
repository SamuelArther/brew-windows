class Ko < Formula
  desc "Build and deploy Go applications on Kubernetes"
  homepage "https://ko.build"
  url "https://github.com/ko-build/ko/archive/refs/tags/v0.19.1.tar.gz"
  sha256 "7accc1f4ad074285086573b084387bef5871872ef16e3f292d5818a99e4feeae"
  license "Apache-2.0"
  head "https://github.com/ko-build/ko.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fb274d07dc2c5c51b3f0d8a1bcd9cc9a82f0cff71c0a0a4a9b8c3b3877273ef9"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X github.com/google/ko/pkg/commands.Version=#{version}")

    generate_completions_from_executable(bin/"ko", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/ko login reg.example.com -u brew -p test 2>&1")
    # Windows prints the path with backslashes.
    config = testpath/".docker/config.json"
    assert_match "logged in via #{OS.windows? ? config.to_s.tr("/", "\\") : config}", output
  end
end
