class PerlXmlParser < Formula
  desc "Perl module for parsing XML documents"
  homepage "https://github.com/cpan-authors/XML-Parser"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-perl-xml-parser-2.59-1-any.pkg.tar.zst"
  sha256 "dae8ba470b1f23dc0107272e50a0a22aa98ad4ec0240e718be2cd7759d238ae3"
  license "Artistic-2.0"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://cpan.metacpan.org/authors/id/T/TO/TODDR/XML-Parser-2.59.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"perl5/site_perl/XML/Parser.pm"
  end
end
