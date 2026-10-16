class CdDiscid < Formula
  desc "Read CD and get CDDB discid information"
  homepage "https://linukz.org/cd-discid.shtml"
  license "GPL-2.0-or-later"
  revision 2
  head "https://github.com/taem/cd-discid.git", branch: "master"

  stable do
    url "https://linukz.org/download/cd-discid-1.4.tar.gz"
    mirror "https://deb.debian.org/debian/pool/main/c/cd-discid/cd-discid_1.4.orig.tar.gz"
    sha256 "ffd68cd406309e764be6af4d5cbcc309e132c13f3597c6a4570a1f218edd2c63"

    # macOS fix; see https://github.com/Homebrew/homebrew/issues/46267
    # Already fixed in upstream head; remove when bumping version to >1.4
    patch do
      file "Patches/cd-discid/1.4.patch"
    end
  end

  # Last commit was 9 years ago, upstream site is gone
  deprecate! date: "2025-10-16", because: :unmaintained
  disable! date: "2026-10-16", because: :unmaintained

  def install
    system "make", "CC=#{ENV.cc}"
    bin.install "cd-discid"
    man1.install "cd-discid.1"
  end

  test do
    assert_equal "cd-discid #{version}.", shell_output("#{bin}/cd-discid --version 2>&1").chomp
  end
end
