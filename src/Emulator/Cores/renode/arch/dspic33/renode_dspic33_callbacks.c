//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//

#include "arch_callbacks.h"
#include "renode_imports.h"

EXTERNAL_AS(int32_t, FindBestInterrupt,     tlib_find_best_interrupt)
EXTERNAL_AS(void,    AcknowledgeInterrupt,  tlib_acknowledge_interrupt, int32_t)
EXTERNAL_AS(void,    OnCpuHalted,           tlib_on_cpu_halted)
EXTERNAL_AS(void,    OnCpuPowerDown,        tlib_on_cpu_power_down)
