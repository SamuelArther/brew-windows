class Solo2Cli < Formula
  desc "CLI to update and use Solo 2 security keys"
  homepage "https://solokeys.com/"
  url "https://github.com/solokeys/solo2-cli/archive/refs/tags/v0.2.2.tar.gz"
  sha256 "49a30c5ee6f38be968a520089741f8b936099611e98e6bf2b25d05e5e9335fb4"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/solokeys/solo2-cli.git", branch: "main"

  deprecate! date: "2026-07-24", because: :repo_archived
  disable! date: "2027-01-24", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "e0f606b812a547bdb4aedc783eb232cd1a78110823a1be6c26990bf1c9d6b1b6"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "pcsc-lite"
    depends_on "systemd"
  end

  # rust 1.79.0 build patch, upstream pr ref, https://github.com/solokeys/solo2-cli/pull/122
  patch do
    url "https://github.com/solokeys/solo2-cli/commit/c4b3f28062860c914f3922ad58604f0bc36ead93.patch?full_index=1"
    sha256 "1f3e08c4c6f17022e8762852ef8e2de94e1c0161d4409d60e5b04f23d72b632d"
    type :unofficial
    resolves "https://github.com/solokeys/solo2-cli/pull/122"
  end

  def install
    system "cargo", "install", "--all-features", *std_cargo_args

    bash_completion.install "target/release/solo2.bash" => "solo2"
    fish_completion.install "target/release/solo2.fish"
    zsh_completion.install "target/release/_solo2"
  end

  test do
    assert_empty shell_output("#{bin}/solo2 ls")
    assert_match version.to_s, shell_output("#{bin}/solo2 --version")
  end
end
