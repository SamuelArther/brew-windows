class Tele < Formula
  desc "Telegram client for the terminal: a keyboard-first TUI and a CLI"
  homepage "https://github.com/sorokin-vladimir/tele"
  url "https://github.com/sorokin-vladimir/tele/archive/refs/tags/v2.1.3.tar.gz"
  sha256 "bae71e369a0b71f0e982677c8b88279db14a8e06fa2a09191eda7d969653c2a7"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4d71fd37deb635452b94027867af5401658a17e2ec3ddbf8f4de266b5ed16499"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X github.com/sorokin-vladimir/tele/internal/version.Version=#{version}"), "./cmd/tele"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tele --version")
    assert_match "dumped from tele-dark", shell_output("#{bin}/tele --theme-dump=tele-dark")
  end
end
