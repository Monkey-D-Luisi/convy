package com.convy.app.ui.demo

import kotlin.test.Test
import kotlin.test.assertEquals

class UiDemoRouteTest {
    @Test
    fun parsesKnownRouteIds() {
        assertEquals(UiDemoRoute.Auth, UiDemoRoute.fromId("auth"))
        assertEquals(UiDemoRoute.HouseholdLists, UiDemoRoute.fromId("household-lists"))
        assertEquals(UiDemoRoute.TaskForm, UiDemoRoute.fromId("task-form"))
    }

    @Test
    fun fallsBackToAuthForUnknownRouteIds() {
        assertEquals(UiDemoRoute.Auth, UiDemoRoute.fromId("missing"))
        assertEquals(UiDemoRoute.Auth, UiDemoRoute.fromId(null))
    }
}
