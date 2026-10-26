class Bibclean < Formula
  desc "BibTeX bibliography file pretty printer and syntax checker"
  homepage "https://www.math.utah.edu/~beebe/software/bibclean/bibclean-03.html#HDR.3"
  url "https://ftp.math.utah.edu/pub/bibclean/bibclean-3.07.tar.xz"
  sha256 "919336782e9e3c204e60f56485fd1f8dd679eb622fc8fd1f4833595ee10191a6"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://ftp.math.utah.edu/pub/bibclean/"
    regex(/href=.*?bibclean[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "bf93ff97698ac7035dba3c52f3e8f768f047ba0545646ca8c7a773a70503bfed"
  end

  deny_network_access!

  def install
    ENV.deparallelize

    system "./configure", "--prefix=#{prefix}",
                          "--mandir=#{man}"

    # The following inline patches have been reported upstream.
    inreplace "Makefile" do |s|
      # Insert `mkdir` statements before `scp` statements because `scp` in macOS
      # requires that the full path to the target already exist.
      s.gsub!(/[$][{]CP.*BIBCLEAN.*bindir.*BIBCLEAN[}]/,
              "mkdir -p ${bindir} && ${CP} ${BIBCLEAN} ${bindir}/${BIBCLEAN}")
      s.gsub!(/[$][{]CP.*bibclean.*mandir.*bibclean.*manext[}]/,
              "mkdir -p ${mandir} && ${CP} bibclean.man ${mandir}/bibclean.${manext}")

      # Correct `mandir` (man file path) in the Makefile.
      s.gsub!(/mandir.*prefix.*man.*man1/, "mandir = ${prefix}/share/man/man1")
    end

    system "make", "all"
    system "make", "install"
  end

  test do
    (testpath/"test.bib").write <<~BIBTEX
      @article{small,
      author = {Test, T.},
      title = {Test},
      journal = {Test},
      year = 2014,
      note = {test},
      }
    BIBTEX

    system bin/"bibclean", "test.bib"
  end
end
