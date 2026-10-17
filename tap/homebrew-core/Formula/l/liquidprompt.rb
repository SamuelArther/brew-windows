class Liquidprompt < Formula
  desc "Adaptive prompt for bash and zsh shells"
  homepage "https://liquidprompt.readthedocs.io/en/stable/"
  url "https://github.com/liquidprompt/liquidprompt/archive/refs/tags/v2.3.0.tar.gz"
  sha256 "47e4af3211cad2c775eb5520926466c37ffc0a50ce7990be5b16f7dd291ef720"
  license "AGPL-3.0-or-later"
  head "https://github.com/liquidprompt/liquidprompt.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/liquidprompt-2.3.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "acd82a36ec6198e2797ac4667a1ca635e2f971e6521d4214022f8a40b63fe394"
  end

  def install
    share.install "liquidprompt"
  end

  def caveats
    <<~EOS
      Add the following lines to your bash or zsh config (e.g. ~/.bash_profile):
        if [ -f #{HOMEBREW_PREFIX}/share/liquidprompt ]; then
          . #{HOMEBREW_PREFIX}/share/liquidprompt
        fi

      If you'd like to reconfigure options, you may do so in ~/.liquidpromptrc.
    EOS
  end

  test do
    liquidprompt = "#{HOMEBREW_PREFIX}/share/liquidprompt"
    output = shell_output("/bin/bash -c '. #{liquidprompt} --no-activate; lp_theme --list'")
    assert_match "default\n", output
  end
end
