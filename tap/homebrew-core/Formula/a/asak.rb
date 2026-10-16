class Asak < Formula
  desc "Cross-platform audio recording/playback CLI tool with TUI"
  homepage "https://github.com/chaosprint/asak"
  url "https://github.com/chaosprint/asak/archive/refs/tags/v0.4.0.tar.gz"
  sha256 "d4df734e87b63cf20f7fa34e15853afa640620043871497d54f78a6818680dd6"
  license "MIT"
  head "https://github.com/chaosprint/asak.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "20c5fe3d03488699b857d1b5969daaaef555b93d41db2dd938a27aede847fbb8"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "alsa-lib"
    depends_on "jack" => :no_linkage
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    bash_completion.install "target/completions/asak.bash" => "asak"
    fish_completion.install "target/completions/asak.fish"
    zsh_completion.install "target/completions/_asak"
    man1.install "target/man/asak.1"
  end

  test do
    output = shell_output("#{bin}/asak play 2>&1", 2)
    assert_match "unexpected argument 'play' found", output

    assert_match version.to_s, shell_output("#{bin}/asak --version")
  end
end
