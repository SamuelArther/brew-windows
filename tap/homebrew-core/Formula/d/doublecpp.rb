class Doublecpp < Formula
  desc "Double dispatch in C++"
  homepage "https://doublecpp.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/doublecpp/doublecpp/0.6.3/doublecpp-0.6.3.tar.gz"
  sha256 "232f8bf0d73795558f746c2e77f6d7cb54e1066cbc3ea7698c4fba80983423af"
  license "GPL-2.0-or-later"

  # Fix build failure because of missing #include <stdlib.h> on Linux.
  # Patch submitted to author by email.
  patch :DATA

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "54b55338986120d309a63bb60bb46a3db62106348ff9a86ce3e10ad18cafd513"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"doublecpp", "--version"
  end
end

__END__
diff --git a/src/branchanalyser.cpp b/src/branchanalyser.cpp
index e6da619..feca23a 100755
--- a/src/branchanalyser.cpp
+++ b/src/branchanalyser.cpp
@@ -9,6 +9,7 @@
  ***************************************************************************/
 
 #include <assert.h>
+#include <stdlib.h>
 
 #include "branchanalyser.h"
 #include "multimethods.h"
diff --git a/src/parambinder.cpp b/src/parambinder.cpp
index b2c77b8..8402221 100755
--- a/src/parambinder.cpp
+++ b/src/parambinder.cpp
@@ -10,6 +10,7 @@
 #include "parambinder.h"
 
 #include <assert.h>
+#include <stdlib.h>
 
 #include "classdecl.h"
 #include "methparams.h"
diff --git a/src/programanalyser.cpp b/src/programanalyser.cpp
index 19e34ba..c69518a 100755
--- a/src/programanalyser.cpp
+++ b/src/programanalyser.cpp
@@ -9,6 +9,8 @@
  ***************************************************************************/
 #include "programanalyser.h"
 
+#include <stdlib.h>
+
 #include "multimethods.h"
 #include "methods.h"
 #include "multimethodtypes.h"
diff --git a/src/sourceanalyser.cpp b/src/sourceanalyser.cpp
index a87fde3..fb6ea5e 100755
--- a/src/sourceanalyser.cpp
+++ b/src/sourceanalyser.cpp
@@ -8,6 +8,8 @@
  *   (at your option) any later version.                                   *
  ***************************************************************************/
 
+#include <stdlib.h>
+
 #include "sourceanalyser.h"
 #include "fileutil.h"
 #include "progelems.hpp"
diff --git a/src/sourcemodifier.cpp b/src/sourcemodifier.cpp
index 5324f6f..84dc15c 100755
--- a/src/sourcemodifier.cpp
+++ b/src/sourcemodifier.cpp
@@ -10,6 +10,7 @@
 
 #include <assert.h>
 #include <iostream> // TODO: remove it when changed ProgElems cons
+#include <stdlib.h>
 
 #include "my_sstream.h"
 
