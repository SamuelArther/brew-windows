class Tele < Formula
  desc "Telegram client for the terminal: a keyboard-first TUI and a CLI"
  homepage "https://github.com/sorokin-vladimir/tele"
  url "https://github.com/sorokin-vladimir/tele/archive/refs/tags/v2.1.1.tar.gz"
  sha256 "9c794adadb60eaf462106b0120624c1d53a4694ca417de2b65a40f358fb25785"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "56110faecfa152c67814348e541c06482f769c5233e8e6fd1933ce767a3738ae"
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
