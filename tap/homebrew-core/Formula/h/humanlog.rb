class Humanlog < Formula
  desc "Logs for humans to read"
  homepage "https://humanlog.io"
  url "https://github.com/humanlogio/humanlog/archive/refs/tags/v0.8.9.tar.gz"
  sha256 "7eb257742d52bd292f61312414af49e85acfe3be6fc687c391fa5fbb8e9e08d4"
  license "Apache-2.0"
  head "https://github.com/humanlogio/humanlog.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "84e1f2cbdeea459102c7ef691cb52cb4f8b43b91ea249b1339856d7523f8e3d1"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    # `go.mod` references a private module that the build does not use
    system "go", "list", "-deps", "./cmd/humanlog"
  end

  def install
    ldflags = %W[
      -X main.versionMajor=#{version.major}
      -X main.versionMinor=#{version.minor}
      -X main.versionPatch=#{version.patch}
      -X main.versionPrerelease=
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/humanlog"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/humanlog --version")

    test_input = '{"time":"2024-12-23T12:34:56Z","level":"info","message":"brewtest log"}'
    expected_output = "brewtest log"

    output = pipe_output(bin/"humanlog", test_input)
    assert_match expected_output, output
  end
end
