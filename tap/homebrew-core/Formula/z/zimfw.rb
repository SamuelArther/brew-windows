class Zimfw < Formula
  desc "Zsh plugin manager"
  homepage "https://zimfw.sh"
  url "https://github.com/zimfw/zimfw/releases/download/v1.20.1/zimfw.zsh.gz"
  sha256 "f8398d723475ae408221d9f04854c5153710d3f29f8fe30edeed733a5f4ca703"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7a012303ae737ee659f82c7c2e513cb79499d7774b5e5ee34904b03970052def"
  end

  uses_from_macos "zsh" => :test

  deny_network_access!

  def install
    share.install "zimfw.zsh"
  end

  def caveats
    <<~EOS
      zimfw.zsh lives in #{opt_share}/zimfw.zsh to source in your .zshrc.
    EOS
  end

  test do
    assert_match version.to_s,
      shell_output("zsh -c 'ZIM_HOME=#{testpath}/.zim source #{share}/zimfw.zsh version'")

    (testpath/".zimrc").write("zmodule test --use mkdir --on-pull '>init.zsh <<<\"print test\"'")
    system "zsh -c 'ZIM_HOME=#{testpath}/.zim source #{share}/zimfw.zsh init -q'"
    assert_path_exists testpath/".zim/modules/test/init.zsh"
    assert_path_exists testpath/".zim/init.zsh"
  end
end
