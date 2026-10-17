class Rig < Formula
  desc "Provides fake name and address data"
  homepage "https://rig.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/rig/rig/1.11/rig-1.11.tar.gz"
  sha256 "00bfc970d5c038c1e68bc356c6aa6f9a12995914b7d4fda69897622cb5b77ab8"
  license "GPL-2.0-or-later"

  conflicts_with "r-rig", because: "both install `rig` binary"

  # Fix build failure because of missing #include <cstring> on Linux.
  # Patch submitted to author by email.
  patch :DATA

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/rig-1.11"
    sha256 cellar: :any, x86_64_windows: "ba8c09782835d094213bbabe6bb82cc1fff39bbf5ecef96f02c8fd2d926996a0"
  end

  def install
    system "make", "PREFIX=#{prefix}"
    bin.install "rig"
    pkgshare.install Dir["data/*"]
  end

  test do
    system bin/"rig"
  end
end

__END__
diff --git a/rig.cc b/rig.cc
index 1f9a2e4..3a23ea8 100644
--- a/rig.cc
+++ b/rig.cc
@@ -21,6 +21,7 @@
 #include <fstream>
 #include <vector>
 #include <string>
+#include <cstring>
 #include <stdlib.h>
 #include <unistd.h>
 #include <time.h>
