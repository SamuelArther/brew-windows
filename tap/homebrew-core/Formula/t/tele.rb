class Tele < Formula
  desc "Keyboard-first Telegram client for the terminal, written in Go"
  homepage "https://github.com/sorokin-vladimir/tele"
  url "https://github.com/sorokin-vladimir/tele/archive/refs/tags/v2.0.2.tar.gz"
  sha256 "ff8586555bed5e6f20ec6f3e85d649acae962107609b1599d91d48a1b1d9f71f"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "501e98ea75479607188bbe5cec97eb2ce2c2008ee526383fdd2f0ea8c259ceba"
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
