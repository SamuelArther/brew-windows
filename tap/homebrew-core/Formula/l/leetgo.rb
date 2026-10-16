class Leetgo < Formula
  desc "CLI tool for LeetCode"
  homepage "https://github.com/j178/leetgo"
  url "https://github.com/j178/leetgo/archive/refs/tags/v1.5.1.tar.gz"
  sha256 "0bcd0b22ec53e527f06d9a12aa8540f0dfa97ba5ff0c5aa0a095002ff7c90c42"
  license "MIT"
  head "https://github.com/j178/leetgo.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8d857349eac5260701766750b636485fb026e00e89ffe496df9e979c3c57bcee"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/j178/leetgo/constants.Version=#{version}
      -X github.com/j178/leetgo/constants.Commit=#{tap.user}
      -X github.com/j178/leetgo/constants.BuildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)
    generate_completions_from_executable(bin/"leetgo", shell_parameter_format: :cobra)
  end

  test do
    assert_match "leetgo version #{version}", shell_output("#{bin}/leetgo --version")
    system bin/"leetgo", "init", "--site", "us"
    assert_path_exists testpath/"leetgo.yaml"
  end
end
