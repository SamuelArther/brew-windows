class Perl < Formula
  desc "Highly capable, feature-rich programming language"
  homepage "https://www.perl.org/"
  url "https://github.com/StrawberryPerl/Perl-Dist-Strawberry/releases/download/SP_54231_64bit/strawberry-perl-5.42.3.1-64bit-portable.zip"
  sha256 "6a081a811781c30aca51dbc036afd93092af91e3297901f02c17043795a10690"
  license any_of: ["Artistic-1.0-Perl", "GPL-1.0-or-later"]

  livecheck do
    url :stable
    regex(/^SP[._-](\d+)[._-]64bit$/i)
    strategy :github_latest
  end

  # perl.org publishes source only and points Windows users to Strawberry Perl. This is its
  # portable zip (no installer). Strawberry has no 5.44 release yet, so this is 5.42.3
  # (homebrew-core: 5.44.0). Its bundled gcc toolchain in libexec/c/bin is not put on PATH.

  def install
    install_tree
    windows_exe "libexec/perl/bin/perl.exe", "perl"
    %w[
      corelist cpan cpanm enc2xs h2ph h2xs instmodsh json_pp libnetcfg perlbug perldoc perlivp
      perlthanks piconv pl2pm pod2html pod2man pod2text pod2usage podchecker prove ptar ptardiff
      ptargrep shasum splain xsubpp zipdetails
    ].each do |cmd|
      windows_cmd "libexec/perl/bin/#{cmd}.bat", cmd
    end
  end

  test do
    (testpath/"test.pl").write "print 'Perl is not an acronym, but JAPH is a Perl acronym!';"
    system bin/"perl", "test.pl"
  end
end
