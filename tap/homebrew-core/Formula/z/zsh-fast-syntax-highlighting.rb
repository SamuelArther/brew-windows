class ZshFastSyntaxHighlighting < Formula
  desc "Feature-rich syntax highlighting for Zsh"
  homepage "https://github.com/zdharma-continuum/fast-syntax-highlighting"
  url "https://github.com/zdharma-continuum/fast-syntax-highlighting/archive/refs/tags/v1.56.tar.gz"
  sha256 "9e5045510ef86beae658b5fcf58e7b6c76f5b63788498b956d54bc1038faa806"
  license "BSD-3-Clause"
  head "https://github.com/zdharma-continuum/fast-syntax-highlighting.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bb21c8cbb2fba175881d9c0579a1c76769d1d0a93b57b0f1a363cd895b79ec76"
  end

  uses_from_macos "zsh" => [:build, :test]

  deny_network_access!

  def install
    # build an `:all` bottle.
    inreplace %w[fast-highlight fast-theme test/parse.zsh test/to-parse.zsh],
              "/usr/local", HOMEBREW_PREFIX
    pkgshare.install Dir["*", ".fast-*"]
  end

  def caveats
    <<~EOS
      To activate the syntax highlighting, add the following at the end of your .zshrc:
        source #{opt_pkgshare}/fast-syntax-highlighting.plugin.zsh
    EOS
  end

  test do
    test_script = testpath/"script.zsh"
    test_script.write <<~ZSH
      #!/usr/bin/env zsh
      source #{pkgshare}/fast-syntax-highlighting.plugin.zsh
      printf '%s' ${FAST_HIGHLIGHT_STYLES+yes}
    ZSH
    assert_match "yes", shell_output("zsh #{test_script}")
  end
end
