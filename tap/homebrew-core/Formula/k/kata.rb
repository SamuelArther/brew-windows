class Kata < Formula
  desc "Local-first, federated issue tracker for humans and coding agents"
  homepage "https://katatracker.com"
  url "https://github.com/kenn-io/kata/releases/download/v0.18.0/kata_0.18.0_source.tar.gz"
  sha256 "980503ab4a5ba37283b5c35013d034cb94e32bc55669426284cbd47030141db6"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dac467bf2bf4f2bd0b31a9ab3474a5708cac1af54988b5321caffc116f243ba7"
  end

  depends_on "go" => :build

  # `test do` block needs network access for `kata init`
  allow_network_access! :test

  def install
    ENV["CGO_ENABLED"] = "0"
    ldflags = %W[
      -X go.kenn.io/kata/internal/version.Version=v#{version}
      -X go.kenn.io/kata/internal/version.Distribution=homebrew
      -X go.kenn.io/kata/internal/version.BuildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:), "-mod=vendor", "-buildvcs=false", "./cmd/kata"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kata version")

    ENV["KATA_HOME"] = testpath/"kata-home"
    ENV["KATA_TELEMETRY_ENABLED"] = "0"
    begin
      system bin/"kata", "init", "--project", "homebrew-test"
      system bin/"kata", "create", "Homebrew test issue"
      assert_match "Homebrew test issue", shell_output("#{bin}/kata list")
    ensure
      system bin/"kata", "daemon", "stop"
    end
  end
end
