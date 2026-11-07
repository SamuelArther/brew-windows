class Swctl < Formula
  desc "Apache SkyWalking CLI (Command-line Interface)"
  homepage "https://skywalking.apache.org/"
  url "https://github.com/apache/skywalking-cli/archive/refs/tags/0.16.0.tar.gz"
  sha256 "712a4f484121b518abde7f98a2a0cc4cc4d03ce0e28f59636376630c8a2aabc1"
  license "Apache-2.0"
  head "https://github.com/apache/skywalking-cli.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1d02340d192e423354cb2d7614502bed744d5622437645209d367cbc00a3b9ce"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/swctl"

    generate_completions_from_executable(bin/"swctl", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/swctl --version 2>&1")

    output = shell_output("#{bin}/swctl --display yaml service ls 2>&1", 1)
    assert_match "level=fatal msg=\"Post \\\"http://127.0.0.1:12800/graphql\\\"", output
  end
end
