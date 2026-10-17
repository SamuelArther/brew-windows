class Cpufetch < Formula
  desc "CPU architecture fetching tool"
  homepage "https://github.com/Dr-Noob/cpufetch"
  url "https://github.com/Dr-Noob/cpufetch/archive/refs/tags/v1.07.tar.gz"
  sha256 "dc3ec8f9c9d41d8434702a778cc150b196d5d178fd768a964f57d22f268a2c17"
  license "GPL-2.0-only"
  head "https://github.com/Dr-Noob/cpufetch.git", branch: "master"

  # Compile with `src/common/sysctl.c` on x86_64 Macs
  # https://github.com/Dr-Noob/cpufetch/issues/375
  patch :DATA

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2e1400ccf8ec42dfb7a459d6b48435e020ea5857c7b04faff200c02cda661018"
  end

  def install
    system "make"
    bin.install "cpufetch"
    man1.install "cpufetch.1"
  end

  test do
    ephemeral_arm = ENV["HOMEBREW_GITHUB_ACTIONS"].present? &&
                    Hardware::CPU.arm? &&
                    OS.mac? &&
                    MacOS.version > :big_sur
    expected_result = ephemeral_arm ? 1 : 0
    actual = shell_output("#{bin}/cpufetch --debug 2>&1", expected_result).lines[0..1].join.strip

    system_name = OS.mac? ? "macOS" : OS.kernel_name
    arch = Hardware::CPU.arm? ? "ARM" : "x86 / x86_64"
    expected = "cpufetch v#{version} (#{system_name} #{arch} build)"

    assert_match expected, actual
  end
end

__END__
diff --git a/Makefile b/Makefile
index d07f036..c88baba 100644
--- a/Makefile
+++ b/Makefile
@@ -20,6 +20,11 @@ ifneq ($(OS),Windows_NT)
 		COMMON_HDR += $(SRC_COMMON)freq.h
 	endif
 
+	ifeq ($(os), Darwin)
+		SOURCE += $(SRC_COMMON)sysctl.c
+		HEADERS += $(SRC_COMMON)sysctl.h
+	endif
+
 	ifeq ($(arch), $(filter $(arch), x86_64 amd64 i386 i486 i586 i686))
 		SRC_DIR=src/x86/
 		SOURCE += $(COMMON_SRC) $(SRC_DIR)cpuid.c $(SRC_DIR)apic.c $(SRC_DIR)cpuid_asm.c $(SRC_DIR)uarch.c
@@ -51,11 +56,6 @@ ifneq ($(OS),Windows_NT)
 		ifeq ($(is_sve_flag_supported), yes)
 			SVE_FLAGS += -march=armv8-a+sve
 		endif
-
-		ifeq ($(os), Darwin)
-			SOURCE += $(SRC_COMMON)sysctl.c
-			HEADERS += $(SRC_COMMON)sysctl.h
-		endif
 	else ifeq ($(arch), $(filter $(arch), riscv64 riscv32))
 		SRC_DIR=src/riscv/
 		SOURCE += $(COMMON_SRC) $(SRC_DIR)riscv.c $(SRC_DIR)uarch.c $(SRC_COMMON)soc.c $(SRC_DIR)soc.c $(SRC_DIR)udev.c
