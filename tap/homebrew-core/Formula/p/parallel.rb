class Parallel < Formula
  desc "Shell command parallelization utility"
  homepage "https://savannah.gnu.org/projects/parallel/"
  url "https://ftpmirror.gnu.org/parallel/parallel-20260922.tar.bz2"
  mirror "https://ftp.gnu.org/gnu/parallel/parallel-20260922.tar.bz2"
  sha256 "54837e9e8c16134a3c2e95f91c5fe2f7893d4cc89f252b26735713ceca866387"
  license "GPL-3.0-or-later"
  version_scheme 1
  head "https://git.savannah.gnu.org/git/parallel.git", branch: "master"

  livecheck do
    url :homepage
    regex(/GNU Parallel v?(\d{6,8}).*? released/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "cdb8973779109ddaa2762d348b34918ba4a55673bcc67c9bcef2f4b1579c0e62"
  end

  conflicts_with "moreutils", because: "both install a `parallel` executable"

  def install
    ENV.append_path "PATH", bin

    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
    bash_completion.install share/"bash-completion/completions/parallel"
  end

  def caveats
    <<~EOS
      To use the --csv option, the Perl Text::CSV module has to be installed.
      You can install it via:
        perl -MCPAN -e'install Text::CSV'
    EOS
  end

  test do
    assert_equal "test\ntest\n",
                 shell_output("#{bin}/parallel --will-cite echo ::: test test")
  end
end
