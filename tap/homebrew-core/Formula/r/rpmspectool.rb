class Rpmspectool < Formula
  desc "Utility for handling RPM spec files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "rpmspectool", "rpmspectool"
  end

  test do
    (testpath/".rpmmacros").write <<~EOS
      %_topdir  %(echo $HOME)/rpmbuild
      %_tmppath %_topdir/tmp
    EOS

    (testpath/"hello.spec").write <<~SPEC
      Name:           hello
      Version:        2.12.1
      Release:        1
      Summary:        Prints a familiar, friendly greeting
      License:        GPL-3.0-or-later AND GFDL-1.3-or-later
      URL:            https://www.gnu.org/software/hello/
      Source0:        https://ftp.gnu.org/gnu/hello/hello-2.12.1.tar.gz

      %description
      The GNU Hello program produces a familiar, friendly greeting.

      %prep
      %setup -q

      %build
      %configure
      %make_build

      %install
      %make_install

      %files -f hello.lang
      %license COPYING
      %_bindir/hello
    SPEC
    assert_equal "Source0: https://ftp.gnu.org/gnu/hello/hello-2.12.1.tar.gz",
                 shell_output("#{bin}/rpmspectool list #{testpath}/hello.spec").strip
    system bin/"rpmspectool", "get", testpath/"hello.spec"
    assert_path_exists testpath/"hello-2.12.1.tar.gz"
  end
end
