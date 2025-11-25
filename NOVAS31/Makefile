# Makefile to build NOVAS C3.1 into a shared library on Linux

CC = gcc
CFLAGS = -fPIC -O2 -Wall -std=c99
LDLIBS = -lm

SRCDIR = NOVAS31
# Collect all C sources in SRCDIR, but exclude Visual Studio and test harness files
ALL_SRCS := $(wildcard $(SRCDIR)/*.c)
EXCLUDE := $(SRCDIR)/example.c \
		   $(SRCDIR)/checkout-stars.c \
		   $(SRCDIR)/checkout-stars-full.c \
		   $(SRCDIR)/checkout-mp.c \
		   $(SRCDIR)/checkout-mp-usno.txt \
		   $(SRCDIR)/checkout-stars-usno.txt \
		   $(SRCDIR)/checkout-stars-full-usno.txt \
		   $(SRCDIR)/solsys2.c \
		   $(SRCDIR)/solsys3.c

SRCS := $(filter-out $(EXCLUDE),$(ALL_SRCS))

# Object filenames placed in build/ to avoid clutter
BUILDDIR = build
OBJS = $(patsubst $(SRCDIR)/%.c,$(BUILDDIR)/%.o,$(SRCS))
PIC_OBJS = $(patsubst $(BUILDDIR)/%.o,$(BUILDDIR)/%.pic.o,$(OBJS))

TARGET = libnovas_c.so

.PHONY: all clean install install-server cio-file

all: $(TARGET) cio-file
	@echo "Built $(TARGET)"

cio-file: cio_ra.bin

# Build the CIO file utility and run it to generate cio_ra.bin
cio_ra.bin: $(SRCDIR)/cio_file.c $(SRCDIR)/CIO_RA.TXT | $(BUILDDIR)
	@echo "Building cio_file utility..."
	$(CC) $(CFLAGS) -o $(BUILDDIR)/cio_file $(SRCDIR)/cio_file.c $(LDLIBS)
	@echo "Generating cio_ra.bin from CIO_RA.TXT..."
	@if [ -f cio_ra.bin ]; then rm cio_ra.bin; fi
	@cp $(SRCDIR)/CIO_RA.TXT .
	@./$(BUILDDIR)/cio_file > /dev/null
	@rm -f CIO_RA.TXT
	@if [ -f cio_ra.bin ]; then \
		echo "Created cio_ra.bin"; \
	else \
		echo "ERROR: Failed to create cio_ra.bin"; \
		exit 1; \
	fi

# Build shared library from PIC objects
$(TARGET): $(PIC_OBJS)
	$(CC) -shared -o $@ $(PIC_OBJS) $(LDLIBS)

# Compile .c -> .o
$(BUILDDIR)/%.o: $(SRCDIR)/%.c | $(BUILDDIR)
	$(CC) $(CFLAGS) -c -o $@ $<

# Create PIC objects from .o (recompile from source to ensure -fPIC)
$(BUILDDIR)/%.pic.o: $(BUILDDIR)/%.o
	$(CC) $(CFLAGS) -c -fPIC -o $@ $(SRCDIR)/$*.c

$(BUILDDIR):
	mkdir -p $(BUILDDIR)

clean:
	rm -rf $(BUILDDIR) $(TARGET) cio_ra.bin

# Install into NINA External folder (relative path from NOVAS31)
SERVER_NOVAS_DIR = ../NINA/External/linux-x64/NOVAS/
SERVER_JPLEPH_DIR = ../NINA/External/

install-server: $(TARGET) cio-file
	@echo "Installing $(TARGET) to $(SERVER_NOVAS_DIR)"
	@mkdir -p $(SERVER_NOVAS_DIR)
	@cp $(TARGET) $(SERVER_NOVAS_DIR)
	@cp cio_ra.bin $(SERVER_NOVAS_DIR)
	@chmod 755 $(SERVER_NOVAS_DIR)$(TARGET) || true
	@echo "Installed: $(SERVER_NOVAS_DIR)$(TARGET)"
	@echo "Installed: $(SERVER_NOVAS_DIR)cio_ra.bin"
	@if [ -f "$(SERVER_JPLEPH_DIR)JPLEPH" ]; then \
		echo "JPLEPH already exists at $(SERVER_JPLEPH_DIR)JPLEPH"; \
	else \
		echo "Note: JPLEPH file not found at $(SERVER_JPLEPH_DIR)JPLEPH"; \
		echo "      JPL ephemeris file must be provided separately"; \
	fi

install: install-server

# Convenience: show what will be built
print-sources:
	@echo "Sources:"
	@printf "  %s\n" $(SRCS)
	@echo "Objects:"
	@printf "  %s\n" $(PIC_OBJS)
