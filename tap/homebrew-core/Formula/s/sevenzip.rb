class Sevenzip < Formula
  desc "7-Zip is a file archiver with a high compression ratio"
  homepage "https://7-zip.org"
  url "https://www.7-zip.org/a/7z2604-x64.msi"
  version "26.04"
  sha256 "0b01334a418654293513449f61d6bfc99e5196ca371e3c3dc961eda57cd535c6"
  license all_of: ["LGPL-2.1-or-later", "BSD-3-Clause"]

  # Official 7-Zip x64 MSI from 7-zip.org, read as an archive (no installer runs).

  def install
    install_tree
    windows_exe "libexec/**/7z.exe", "7z"
  end

  test do
    (testpath/"foo.txt").write("hello world!\n")
    system bin/"7z", "a", "-t7z", "foo.7z", "foo.txt"
    system bin/"7z", "e", "foo.7z", "-oout"
    assert_equal "hello world!\n", (testpath/"out/foo.txt").read
  end
end
