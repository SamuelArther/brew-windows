class Nvimpager < Formula
  desc "Use NeoVim as a pager to view manpages, diffs, etc."
  homepage "https://github.com/lucc/nvimpager"
  url "https://github.com/lucc/nvimpager/archive/refs/tags/v0.14.0.tar.gz"
  sha256 "001c49098690e4baa6857557b9edc564433825a1947ec368a1e52d63aa1715a3"
  license "BSD-2-Clause"
  head "https://github.com/lucc/nvimpager.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b7aad4d3b003820d1c1ab3a68c5f084077d1a28c1d411cade9ac170013004eed"
  end

  depends_on "scdoc" => :build
  depends_on "neovim"

  def install
    system "make", "PREFIX=#{prefix}", "install"
  end

  def caveats
    <<~EOS
      To use nvimpager as your default pager, add `export PAGER=nvimpager`
      to your shell configuration.
    EOS
  end

  test do
    (testpath/"test.txt").write <<~EOS
      This is test
    EOS

    assert_match(/This is test/, shell_output("#{bin}/nvimpager test.txt"))
  end
end
