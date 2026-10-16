class Vcs < Formula
  desc "Creates video contact sheets (previews) of videos"
  homepage "https://web.archive.org/web/20240824173847/https://p.outlyer.net/vcs/"
  url "https://web.archive.org/web/20240824173847/https://p.outlyer.net/files/vcs/vcs-1.13.4.tar.gz"
  sha256 "dc1d6145e10eeed61d16c3591cfe3496a6ac392c9c2f7c2393cbdb0cf248544b"
  license "LGPL-2.0-or-later"
  revision 3

  deprecate! date: "2025-03-22", because: :repo_removed
  disable! date: "2026-03-22", because: :repo_removed

  depends_on "ffmpeg"
  depends_on "ghostscript"
  depends_on "imagemagick"

  on_macos do
    depends_on "gnu-getopt"
  end

  def install
    inreplace "vcs", "declare GETOPT=getopt", "declare GETOPT=#{formula_opt_bin("gnu-getopt")}/getopt" if OS.mac?

    system "make", "install", "prefix=#{prefix}"
  end

  test do
    system formula_opt_bin("ffmpeg")/"ffmpeg", "-f", "rawvideo", "-s", "hd720",
           "-pix_fmt", "yuv420p", "-r", "30", "-t", "5", "-i", "/dev/zero",
           testpath/"video.mp4"
    assert_path_exists testpath/"video.mp4"

    system bin/"vcs", "-i", "1", "-o", testpath/"sheet.png", testpath/"video.mp4"
    assert_path_exists testpath/"sheet.png"
  end
end
