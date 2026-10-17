class Taskline < Formula
  desc "Tasks, boards & notes for the command-line habitat"
  homepage "https://github.com/perryrh0dan/taskline"
  url "https://registry.npmjs.org/@perryrh0dan/taskline/-/taskline-1.6.0.tgz"
  sha256 "b27122b0578c6890342ec3742ef14f89aca4315853282f982c1b6dad6b6faba2"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aa27b21b576313ac0fd4f424707cab63bc860e276390c59ade3d3656b1f504f7"
  end

  depends_on "node"

  on_linux do
    depends_on "xsel"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    clipboardy_fallbacks_dir = libexec/"lib/node_modules/@perryrh0dan/#{name}/node_modules/clipboardy/fallbacks"
    rm_r(clipboardy_fallbacks_dir) # remove pre-built binaries

    linux_dir = clipboardy_fallbacks_dir/"linux"
    linux_dir.mkpath
    # Replace the vendored pre-built xsel with one we build ourselves.
    # We add unused symlink on macOS to create an all bottle
    ln_sf (formula_opt_bin("xsel")/"xsel").relative_path_from(linux_dir), linux_dir
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tl --version")

    assert_match "Available Boards:", shell_output("#{bin}/tl boards")
  end
end
